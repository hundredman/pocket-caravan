-- Whole-source native preparation. Never called from the live map generator.
local root=assert(app.params.root);local pc=app.pixelColor
local defs={{'boat',80,32,77,30,30},{'sheaf_naru',16,32,16,25,31},{'laundry',32,32,32,21,30},{'sheaf_jangdan',16,32,16,25,31},{'pine_west',64,48,64,47,47},{'round_west',64,48,64,47,47},{'small_store',64,48,64,48,48},{'small_rock',32,32,31,22,32},{'pine_east',64,48,64,47,47},{'round_east',64,48,64,47,47},{'bench',48,16,48,16,16},{'lotus',32,16,31,15,16}}
for _,d in ipairs(defs)do
 local name,w,h,mw,mh,bottom=table.unpack(d)
 if app.params.finish~='true'then
  local s=app.open(root..'/AI/'..name..'.png');local im=Image(s.spec);im:drawSprite(s,1)
  local x0,y0,x1,y1=im.width,im.height,-1,-1
  for y=0,im.height-1 do for x=0,im.width-1 do if pc.rgbaA(im:getPixel(x,y))>=224 then x0=math.min(x0,x);y0=math.min(y0,y);x1=math.max(x1,x);y1=math.max(y1,y)end end end
  assert(x1>=x0,'Empty '..name);local scale=math.min(mw/(x1-x0+1),mh/(y1-y0+1));local dw,dh=math.floor((x1-x0+1)*scale+.5),math.floor((y1-y0+1)*scale+.5)
  local out=Sprite(w,h,ColorMode.RGB);local dst=out.cels[1].image;local ox,oy=math.floor((w-dw)/2),bottom-dh
  for y=0,dh-1 do for x=0,dw-1 do
   local v=im:getPixel(x0+math.min(x1-x0,math.floor((x+.5)/dw*(x1-x0+1))),y0+math.min(y1-y0,math.floor((y+.5)/dh*(y1-y0+1))))
   if pc.rgbaA(v)>=224 then dst:drawPixel(ox+x,oy+y,pc.rgba(pc.rgbaR(v),pc.rgbaG(v),pc.rgbaB(v),255))end
  end end
  out:saveCopyAs(root..'/Prepared/'..name..'-seed.png');out:close();s:close();print(name..' '..dw..'x'..dh)
 else
  local s=app.open(root..'/Prepared/'..name..'.png');s.layers[1].name='Complete AI geometry, A5 candidate colors';local im=s.cels[1].image
  if name=='sheaf_naru'or name=='sheaf_jangdan'then
   -- Match the original stocky16x25 cut-straw bundle, not an hourglass stalk.
   s.layers[1].isVisible=false;im=Image(16,32,ColorMode.RGB)
   local function rgb(h)return pc.rgba(tonumber(h:sub(1,2),16),tonumber(h:sub(3,4),16),tonumber(h:sub(5,6),16),255)end
   for y=6,30 do
    local x0,x1=2,13
    if y<=9 then x0,x1=3,12 elseif y>=11 and y<=17 then x0,x1=1,14 elseif y>=21 and y<=26 then x0,x1=1,14 elseif y==27 then x0,x1=0,15 elseif y==28 then x0,x1=1,14 elseif y==30 then x0,x1=3,12 end
    for x=x0,x1 do
     local h=x<8 and 'daa34a'or'b78137'
     if x==3 or x==6 or x==10 then h='f0c365'end
     if y==18 or y==19 then h=name=='sheaf_naru'and'7d9156'or'bc8850' end
     -- Irregular cut straw tips and splayed cut ends, no long narrow neck.
     local tipGap=y==6 and not(x==4 or x==7 or x==11)or y==7 and(x==3 or x==6 or x==9 or x==12)
     local endGap=y==30 and(x==6 or x==7 or x==11)
     if not(tipGap or endGap)then im:drawPixel(x,y,rgb(h))end
    end
   end
   for _,p in ipairs({{3,10},{3,11},{4,12},{6,14},{6,15},{11,12},{11,13},{5,23},{5,24},{10,25},{10,26}})do im:drawPixel(p[1],p[2],rgb('e0c99b'))end
   local native=s:newLayer();native.name='Native full tied straw body, original16x25 silhouette';s:newCel(native,1,im,Point(0,0))
  end
  local edge=Image(w,h,ColorMode.RGB)
  for y=0,h-1 do for x=0,w-1 do if pc.rgbaA(im:getPixel(x,y))>0 then
   for _,q in ipairs({{1,0},{-1,0},{0,1},{0,-1}})do local xx,yy=x+q[1],y+q[2]
    if xx<0 or yy<0 or xx>=w or yy>=h or pc.rgbaA(im:getPixel(xx,yy))==0 then edge:drawPixel(x,y,pc.rgba(46,36,29,255));break end
   end
  end end end
  local layer=s:newLayer();layer.name='Native one pixel exterior contour';s:newCel(layer,1,edge,Point(0,0));s:saveAs(root..'/Native/'..name..'.aseprite');s:saveCopyAs(root..'/Native/'..name..'.png');s:close()
 end
end
