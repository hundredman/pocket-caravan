-- C7 native Aseprite hand pass. AI roof/facade design is redrawn on the
-- original 64x48 house envelope instead of stretching the generated image.
-- A5-quantized pine is retained whole; only its outer contour is unified.
local root=assert(app.params.root)
local pc=app.pixelColor
local P={ink='34271f',wood='a56b3f',woodlight='cc9256',wooddark='70452f',wall='f1dfad',wallshade='d5bd89',straw='e5b355',strawlight='ffda7c',strawdark='bf873d',roof='6f8d9e',rooflight='a0b6bd',roofdark='496777',leaf='65874b',leaflight='96b55f',leafdark='3e6345',grass='8ba865',grassshade='789653',dirt='cfaa70',dirtshade='b38c58'}
local function color(n) local h=P[n] or n;return pc.rgba(tonumber(h:sub(1,2),16),tonumber(h:sub(3,4),16),tonumber(h:sub(5,6),16),255) end
local function save(s,n) s:saveAs(root..'/Native/'..n..'.aseprite');s:saveCopyAs(root..'/Native/'..n..'.png');s:close() end
local function house(name)
 local s=app.open(root..'/Prepared/'..name..'.png')
 s.layers[1].name='A5 AI reference (hidden)';s.layers[1].isVisible=false
 local layer=s:newLayer();layer.name='Native redraw from AI concept; original envelope'
 local im=Image(64,48,ColorMode.RGB);s:newCel(layer,1,im,Point(0,0));im=s.cels[#s.cels].image
 local function dot(x,y,c) if x>=0 and y>=0 and x<64 and y<48 then im:drawPixel(x,y,color(c)) end end
 local function line(x0,y0,x1,y1,c)
  local dx,dy=math.abs(x1-x0),-math.abs(y1-y0);local sx=x0<x1 and 1 or -1;local sy=y0<y1 and 1 or -1;local e=dx+dy
  while true do dot(x0,y0,c);if x0==x1 and y0==y1 then break end;local e2=2*e;if e2>=dy then e=e+dy;x0=x0+sx end;if e2<=dx then e=e+dx;y0=y0+sy end end
 end
 local function rect(x0,y0,x1,y1,c) for y=y0,y1 do for x=x0,x1 do dot(x,y,c) end end end
 local function poly(pts,c,edge)
  for y=0,47 do for x=0,63 do
   local inside=false;local j=#pts
   for i=1,#pts do local a,b=pts[i],pts[j];if (a[2]>y)~=(b[2]>y) and x<(b[1]-a[1])*(y-a[2])/(b[2]-a[2])+a[1] then inside=not inside end;j=i end
   if inside then dot(x,y,c) end
  end end
  if edge then for i=1,#pts do local a,b=pts[i],pts[i%#pts+1];line(a[1],a[2],b[1],b[2],edge) end end
 end
 rect(8,22,55,46,'ink');rect(9,24,54,45,'wall');rect(9,40,54,45,'wallshade')
 for _,x in ipairs({9,29,52}) do rect(x,24,x+2,45,'wooddark');line(x,25,x,44,'woodlight') end
 rect(10,43,53,45,'wood');rect(25,29,38,46,'ink');rect(26,30,37,45,'wood');rect(27,30,28,44,'woodlight');line(32,31,32,44,'wooddark');dot(35,38,'ink')
 for _,x in ipairs({13,41}) do rect(x,28,x+9,36,'wooddark');rect(x+1,29,x+8,34,'wall');line(x+3,29,x+3,34,'wood');line(x+6,29,x+6,34,'wood');line(x,36,x+9,36,'woodlight') end
 if name=='thatch' then
  poly({{2,22},{5,15},{13,5},{20,2},{45,2},{52,6},{59,16},{62,24},{57,27},{7,27},{2,25}},'strawdark','ink')
  poly({{5,22},{8,15},{15,6},{21,4},{44,4},{50,8},{57,19},{59,24},{7,24}},'straw')
  poly({{8,17},{16,7},{22,5},{43,5},{49,8},{53,14},{15,14}},'strawlight')
  line(8,17,55,17,'strawdark');line(15,7,10,23,'wood');line(29,4,28,24,'wood');line(44,4,47,24,'wood')
 else
  poly({{0,24},{5,22},{11,13},{14,6},{14,1},{17,3},{19,5},{44,5},{47,3},{49,1},{49,7},{54,16},{60,22},{60,25},{49,24},{42,22},{20,22},{10,25},{0,26}},'roofdark','ink')
  poly({{4,23},{12,13},{16,6},{20,7},{44,7},{48,5},{49,12},{56,21},{49,21},{41,19},{21,19},{12,22}},'roof')
  line(17,5,45,5,'rooflight');line(15,6,11,17,'rooflight');line(23,8,20,20,'rooflight');line(32,8,32,19,'rooflight');line(41,8,44,20,'rooflight');line(48,9,53,19,'rooflight')
  line(13,15,50,15,'roofdark');line(5,23,12,22,'rooflight');line(50,23,58,24,'rooflight')
 end
 save(s,name)
end
house('thatch');house('tile-house')
local pine=app.open(root..'/Prepared/pine.png');pine.layers[1].name='A5 AI pine, contour hand pass'
local im=pine.cels[1].image;local edits={}
-- The shared trial palette has terrain greens too. Restrict foliage to exactly
-- the three proposed leaf colors rather than leaking terrain shades into it.
for y=0,im.height-1 do for x=0,im.width-1 do
 local c=im:getPixel(x,y);local r,g,b=pc.rgbaR(c),pc.rgbaG(c),pc.rgbaB(c)
 if pc.rgbaA(c)>0 and g>r and g>b then
  local best,score=nil,math.huge
  for _,n in ipairs({'leaf','leaflight','leafdark'}) do local v=color(n);local d=(r-pc.rgbaR(v))^2+(g-pc.rgbaG(v))^2+(b-pc.rgbaB(v))^2;if d<score then best,score=v,d end end
  im:drawPixel(x,y,best)
 end
end end
for y=0,im.height-1 do for x=0,im.width-1 do
 if pc.rgbaA(im:getPixel(x,y))>0 then
  local boundary=false
  for _,d in ipairs({{0,1},{1,0},{0,-1},{-1,0}}) do local xx,yy=x+d[1],y+d[2];if xx<0 or yy<0 or xx>=im.width or yy>=im.height or pc.rgbaA(im:getPixel(xx,yy))==0 then boundary=true end end
  if boundary then table.insert(edits,{x,y}) end
 end
end end
for _,p in ipairs(edits) do im:drawPixel(p[1],p[2],color('ink')) end
save(pine,'pine')
-- Purpose-designed boundary kit: every corner quadrant is authored to its
-- three neighboring grass cells. All 47 legal masks are exported in full.
local function has(mask,bit) return (mask & bit)~=0 end
for mask=0,255 do
 local n,e,so,w=has(mask,1),has(mask,2),has(mask,4),has(mask,8)
 local valid=(not has(mask,16) or (n and e)) and (not has(mask,32) or (e and so)) and (not has(mask,64) or (so and w)) and (not has(mask,128) or (w and n))
 if valid then
  local s=Sprite(16,16,ColorMode.RGB);s.layers[1].name='Authored grass-dirt boundary mask '..mask;local im=s.cels[1].image
  for y=0,15 do for x=0,15 do
   local grass=true
   if not n and y<2 then grass=false end;if not so and y>=14 then grass=false end
   if not w and x<2 then grass=false end;if not e and x>=14 then grass=false end
   if n and e and not has(mask,16) and x>=13 and y<3 then grass=false end
   if e and so and not has(mask,32) and x>=13 and y>=13 then grass=false end
   if so and w and not has(mask,64) and x<3 and y>=13 then grass=false end
   if w and n and not has(mask,128) and x<3 and y<3 then grass=false end
   im:drawPixel(x,y,color(grass and 'grass' or 'dirt'))
  end end
  -- sparse motif from generated grass source; no random noise or per-tile outline
  im:drawPixel(7,7,color('grassshade'));im:drawPixel(8,6,color('grassshade'))
  save(s,'grass-'..mask)
 end
end
local dirt=Sprite(16,16,ColorMode.RGB);dirt.layers[1].name='A5 earth concept, flat native pass'
for y=0,15 do for x=0,15 do dirt.cels[1].image:drawPixel(x,y,color('dirt')) end end
dirt.cels[1].image:drawPixel(5,10,color('dirtshade'));dirt.cels[1].image:drawPixel(6,10,color('dirtshade'));save(dirt,'dirt')
print('C7 native house redraws, whole AI pine outline pass, 47-mask boundary kit + dirt saved; approval pending')
