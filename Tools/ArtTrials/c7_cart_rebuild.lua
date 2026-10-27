-- Authored at final 32x32 grid. AI image is composition reference, never shrunk into art.
local root=assert(app.params.root);local pc=app.pixelColor
local function c(h)return pc.rgba(tonumber(h:sub(1,2),16),tonumber(h:sub(3,4),16),tonumber(h:sub(5,6),16),255)end
local P={ink=c('2e241d'),dark=c('68452d'),wood=c('98653b'),light=c('bc8850'),rim=c('daa34a'),hub=c('f0c365'),wheel=c('77746c')}
local s=Sprite(32,32,ColorMode.RGB)
local function line(im,x0,y0,x1,y1,col,r)
 local dx,dy=math.abs(x1-x0),-math.abs(y1-y0);local sx=x0<x1 and 1 or -1;local sy=y0<y1 and 1 or -1;local e=dx+dy
 while true do
  for y=y0-(r or 0),y0+(r or 0)do for x=x0-(r or 0),x0+(r or 0)do if x>=0 and y>=0 and x<32 and y<32 then im:drawPixel(x,y,col)end end end
  if x0==x1 and y0==y1 then break end;local ee=2*e
  if ee>=dy then e=e+dy;x0=x0+sx end;if ee<=dx then e=e+dx;y0=y0+sy end
 end
end
local function polygon(im,points,col)
 for y=0,31 do for x=0,31 do
  local inside=false;local j=#points
  for i=1,#points do local a,b=points[i],points[j];if ((a[2]>y)~=(b[2]>y))and x<(b[1]-a[1])*(y-a[2])/(b[2]-a[2])+a[1]then inside=not inside end;j=i end
  if inside then im:drawPixel(x,y,col)end
 end end
 for i=1,#points do local a,b=points[i],points[i%#points+1];line(im,a[1],a[2],b[1],b[2],P.ink)end
end
local function wheel(im,cx,cy,r)
 for y=cy-r,cy+r do for x=cx-r,cx+r do
  local d=(x-cx)^2+(y-cy)^2
  if d<=r*r then
   local col=d>(r-1)^2 and P.ink or P.wheel
   if math.abs(x-cx)==0 or math.abs(y-cy)==0 then col=P.ink end
   if d==0 then col=P.hub end
   im:drawPixel(x,y,col)
  end
 end end
end
local back=s.layers[1];back.name='Parallel shafts and visible far wheel';local under=s.cels[1].image
line(under,3,18,12,15,P.ink,1);line(under,3,18,12,15,P.rim)
line(under,7,24,17,21,P.ink,1);line(under,7,24,17,21,P.rim)
wheel(under,18,10,3)
local body=s:newLayer();body.name='Shallow cargo box, single perspective';local box=Image(32,32,ColorMode.RGB)
polygon(box,{{11,13},{23,8},{29,13},{17,18}},P.rim)
-- Dark empty opening reads as one broad plane, not individual planks.
polygon(box,{{13,13},{23,10},{26,13},{17,16}},P.dark)
polygon(box,{{11,13},{17,18},{17,24},{11,19}},P.light)
polygon(box,{{17,18},{29,13},{29,20},{17,24}},P.wood)
line(box,18,18,28,14,P.rim);line(box,12,14,16,18,P.rim)
line(box,19,21,27,18,P.light)
s:newCel(body,1,box,Point(0,0))
local near=s:newLayer();near.name='Near wheel and readable hub';local front=Image(32,32,ColorMode.RGB)
wheel(front,25,25,4);s:newCel(near,1,front,Point(0,0))
s:saveAs(root..'/Native/cart.aseprite');s:saveCopyAs(root..'/Prepared/cart-seed.png');s:close()
