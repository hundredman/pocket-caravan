-- Native redraw of complete source objects; no fragment reuse or runtime edits.
local root=assert(app.params.root);local base=assert(app.params.base);local pc=app.pixelColor
local function c(h)return pc.rgba(tonumber(h:sub(1,2),16),tonumber(h:sub(3,4),16),tonumber(h:sub(5,6),16),255)end
if app.params.seed=='true'then
 local input=app.open(root..'/AI/harvest.png');local img=Image(input.spec);img:drawSprite(input,1)
 local x0,y0,x1,y1=img.width,img.height,-1,-1
 for y=0,img.height-1 do for x=0,img.width-1 do if pc.rgbaA(img:getPixel(x,y))>=224 then x0=math.min(x0,x);y0=math.min(y0,y);x1=math.max(x1,x);y1=math.max(y1,y)end end end
 assert(x1>=x0,'Empty edited cluster');local scale=math.min(41/(x1-x0+1),24/(y1-y0+1));local w,h=math.floor((x1-x0+1)*scale+.5),math.floor((y1-y0+1)*scale+.5)
 local out=Sprite(48,32,ColorMode.RGB);local dst=out.cels[1].image;local ox,oy=math.floor((48-w)/2),31-h
 for y=0,h-1 do for x=0,w-1 do
  local v=img:getPixel(x0+math.min(x1-x0,math.floor((x+.5)/w*(x1-x0+1))),y0+math.min(y1-y0,math.floor((y+.5)/h*(y1-y0+1))))
  if pc.rgbaA(v)>=224 then dst:drawPixel(ox+x,oy+y,pc.rgba(pc.rgbaR(v),pc.rgbaG(v),pc.rgbaB(v),255))end
 end end
 out:saveCopyAs(root..'/Prepared/harvest-seed.png');out:close();input:close();return
end
for _,name in ipairs({'harvest','celadon'})do
 local s=app.open(base..'/ArtReview/C7-WorkshopProps-v3-20261027/Native/'..name..'.aseprite')
 local original=Image(s.spec);original:drawSprite(s,1);local im=Image(original)
 local function put(x,y,h)im:drawPixel(x,y,c(h))end
 local function row(y,x0,x1,h)for x=x0,x1 do put(x,y,h)end end
 if name=='harvest'then
  -- Edit the complete object while keeping its own foreground wares in place.
  local edited=app.open(root..'/Prepared/harvest.png');local replacement=Image(edited.spec);replacement:drawSprite(edited,1)
  for y=7,28 do for x=3,34 do
   local foreground=(x>=17 and x<=27 and y>=18)or(x>=28 and y>=21)
   if not foreground then im:drawPixel(x,y,replacement:getPixel(x,y))end
  end end
  edited:close()
 else
  -- Center axis x14 for rim, neck and both shoulders. No one-sided lip glint.
  for y=7,20 do for x=9,18 do im:drawPixel(x,y,pc.rgba(0,0,0,0))end end
  for y=7,20 do
   local r=y<=8 and 2 or (y<=12 and 1 or (y<=15 and 2 or (y<=17 and 3 or 4)))
   for x=14-r,14+r do
    local edge=x==14-r or x==14+r or y==7
    local color=edge and '2e241d' or (x<=13 and 'b6cfac' or (x<=15 and '86a28b' or '547e6a'))
    if y==8 and not edge then color='304d47'end
    if y>=9 and y<=12 and x==14 then color='86a28b'end
    put(x,y,color)
   end
  end
 end
 for _,l in ipairs(s.layers)do l.isVisible=false end
 local layer=s:newLayer();layer.name=name=='harvest' and 'Short broad tied sheaves, sack and basket retained' or 'Vertical x14 mouth neck and shoulder axis';s:newCel(layer,1,im,Point(0,0))
 local edge=Image(48,32,ColorMode.RGB)
 for y=0,31 do for x=0,47 do if pc.rgbaA(im:getPixel(x,y))>0 then
  for _,d in ipairs({{1,0},{-1,0},{0,1},{0,-1}})do local xx,yy=x+d[1],y+d[2]
   if xx<0 or yy<0 or xx>=48 or yy>=32 or pc.rgbaA(im:getPixel(xx,yy))==0 then edge:drawPixel(x,y,c('2e241d'));break end
  end
 end end end
 local outline=s:newLayer();outline.name='Native exterior contour';s:newCel(outline,1,edge,Point(0,0))
 s:saveAs(root..'/Native/'..name..'.aseprite');s:saveCopyAs(root..'/Native/'..name..'.png');s:close()
end
