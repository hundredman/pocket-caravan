-- Whole object sampling and native exterior contour, never runtime edits.
local root=assert(app.params.root);local pc=app.pixelColor
local defs={{'forest_round',48,64,46,49,63},{'forest_bent',48,64,46,47,63},{'forest_wide',64,48,53,46,47}}
for _,d in ipairs(defs)do
 local name,w,h,mw,mh,bottom=table.unpack(d)
 if app.params.finish~='true'then
  local s=app.open(root..'/AI/'..name..'.png');local im=s.cels[1].image;local x0,y0,x1,y1=im.width,im.height,-1,-1
  for y=0,im.height-1 do for x=0,im.width-1 do if pc.rgbaA(im:getPixel(x,y))>=224 then x0=math.min(x0,x);y0=math.min(y0,y);x1=math.max(x1,x);y1=math.max(y1,y)end end end
  assert(x1>=x0,'Empty '..name)
  local scale=math.min(mw/(x1-x0+1),mh/(y1-y0+1));local dw,dh=math.floor((x1-x0+1)*scale+.5),math.floor((y1-y0+1)*scale+.5)
  local ox,oy=math.floor((w-dw)/2),bottom-dh;local out=Sprite(w,h,ColorMode.RGB);local dst=out.cels[1].image
  for y=0,dh-1 do for x=0,dw-1 do
   local sx=x0+math.min(x1-x0,math.floor((x+.5)/dw*(x1-x0+1)));local sy=y0+math.min(y1-y0,math.floor((y+.5)/dh*(y1-y0+1)))
   local c=im:getPixel(sx,sy);if pc.rgbaA(c)>=224 then dst:drawPixel(ox+x,oy+y,pc.rgba(pc.rgbaR(c),pc.rgbaG(c),pc.rgbaB(c),255))end
  end end
  out:saveCopyAs(root..'/Prepared/'..name..'-seed.png');out:close();s:close();print(name..' '..dw..'x'..dh..' bottom '..bottom)
 else
  local s=app.open(root..'/Prepared/'..name..'.png');s.layers[1].name='Whole AI geometry / A5 candidate colors';local base=s.cels[1].image
  local layer=s:newLayer();layer.name='Native one pixel contour';local im=Image(w,h,ColorMode.RGB)
  for y=0,h-1 do for x=0,w-1 do if pc.rgbaA(base:getPixel(x,y))>0 then
   local edge=false
   for _,q in ipairs({{1,0},{-1,0},{0,1},{0,-1}})do local xx,yy=x+q[1],y+q[2];if xx<0 or yy<0 or xx>=w or yy>=h or pc.rgbaA(base:getPixel(xx,yy))==0 then edge=true end end
   if edge then im:drawPixel(x,y,pc.rgba(46,36,29,255))end
  end end end
  s:newCel(layer,1,im,Point(0,0));s:saveAs(root..'/Native/'..name..'.aseprite');s:saveCopyAs(root..'/Native/'..name..'.png');s:close()
 end
end
