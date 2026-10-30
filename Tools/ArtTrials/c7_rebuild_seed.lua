-- Native authoring, not a map transform. The entire AI object is retained;
-- trim only transparent padding and uniformly sample the complete silhouette.
local root=assert(app.params.root)
local pc=app.pixelColor
for _,name in ipairs({'thatch','tile-house','ground-source'}) do
 local src=app.open(root..'/AI/'..name..'.png');local im=src.cels[1].image
 local ground=name=='ground-source';local x0,y0,x1,y1=im.width,im.height,-1,-1
 for y=0,im.height-1 do for x=0,im.width-1 do
  if pc.rgbaA(im:getPixel(x,y))>=128 then x0=math.min(x0,x);y0=math.min(y0,y);x1=math.max(x1,x);y1=math.max(y1,y) end
 end end
 local w,h=64,48
 if ground then w,h=32,16;x0,y0,x1,y1=0,0,im.width-1,im.height-1 end
 local scale=math.min((w-4)/(x1-x0+1),(h-2)/(y1-y0+1))
 local dw,dh=math.floor((x1-x0+1)*scale+.5),math.floor((y1-y0+1)*scale+.5)
 local ox,oy=math.floor((w-dw)/2),h-1-dh
 if ground then
  assert(math.abs(im.width/im.height-2)<.002,'Ground source must preserve 2:1 aspect')
  ox,oy,dw,dh=0,0,w,h
 end
 local out=Sprite(w,h,ColorMode.RGB);out.layers[1].name='Whole AI design sampled on native grid'
 local dst=out.cels[1].image
 for y=0,dh-1 do for x=0,dw-1 do
  local sx=x0+math.min(x1-x0,math.floor((x+.5)/dw*(x1-x0+1)))
  local sy=y0+math.min(y1-y0,math.floor((y+.5)/dh*(y1-y0+1)))
  local c=im:getPixel(sx,sy)
  if pc.rgbaA(c)>=128 then dst:drawPixel(ox+x,oy+y,pc.rgba(pc.rgbaR(c),pc.rgbaG(c),pc.rgbaB(c),255)) end
 end end
 out:saveCopyAs(root..'/Prepared/'..name..'-seed.png');out:close();src:close()
 print('WHOLE_NATIVE '..name..' '..dw..'x'..dh..' at '..ox..','..oy)
end
