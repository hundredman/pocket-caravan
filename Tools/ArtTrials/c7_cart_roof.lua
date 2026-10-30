-- Whole-object preparation and native source authoring. Never applied at runtime.
local root=assert(app.params.root);local base=assert(app.params.base);local pc=app.pixelColor
local function c(h)return pc.rgba(tonumber(h:sub(1,2),16),tonumber(h:sub(3,4),16),tonumber(h:sub(5,6),16),255)end
local function export(s,n) s:saveAs(root..'/Native/'..n..'.aseprite');s:saveCopyAs(root..'/Native/'..n..'.png');s:close()end
if app.params.finish~='true' then
 local s=app.open(root..'/AI/cart.png');local im=s.cels[1].image;local x0,y0,x1,y1=im.width,im.height,-1,-1
 for y=0,im.height-1 do for x=0,im.width-1 do if pc.rgbaA(im:getPixel(x,y))>=224 then x0=math.min(x0,x);y0=math.min(y0,y);x1=math.max(x1,x);y1=math.max(y1,y)end end end
 assert(x1>=x0,'Empty cart');local scale=math.min(28/(x1-x0+1),27/(y1-y0+1));local dw,dh=math.floor((x1-x0+1)*scale+.5),math.floor((y1-y0+1)*scale+.5)
 local out=Sprite(32,32,ColorMode.RGB);local dest=out.cels[1].image;local ox,oy=math.floor((32-dw)/2),30-dh
 for y=0,dh-1 do for x=0,dw-1 do
  local v=im:getPixel(x0+math.min(x1-x0,math.floor((x+.5)/dw*(x1-x0+1))),y0+math.min(y1-y0,math.floor((y+.5)/dh*(y1-y0+1))))
  if pc.rgbaA(v)>=224 then dest:drawPixel(ox+x,oy+y,pc.rgba(pc.rgbaR(v),pc.rgbaG(v),pc.rgbaB(v),255))end
 end end
 out:saveCopyAs(root..'/Prepared/cart-seed.png');out:close();s:close();print('cart complete bounds '..dw..'x'..dh)
 -- Roof reference is generated separately; author its organized tile rhythm at
 -- native resolution on the WHOLE existing house, retaining every other pixel.
 local house=app.open(base..'/ArtReview/Approved/C7-Buildings-v5/tile-house.png');local im=house.cels[1].image
 local blue={[c('4a627a')]=true,[c('718da3')]=true,[c('a5b9c3')]=true};local original=Image(im)
 for y=14,26 do for x=0,63 do
  if blue[original:getPixel(x,y)]then
   local v=c('4a627a');local spread=.68+.026*(y-14)
   local courseY=y<=19 and 16 or 21
   local step=y-courseY
   for offset=-24,24,6 do
    local xx=math.floor(32+offset*spread+.5)
    -- Curved lit shoulder, short overlap seam: no full-width grid line.
    if step>=0 and step<=3 and x==xx+1+(step==0 and 1 or 0)then v=c('718da3')end
    if step==0 and x==xx+2 then v=c('a5b9c3')end
    if step==3 and x>=xx+1 and x<=xx+3 then v=c('33475e')end
    if step>=1 and step<=2 and x==xx+4 then v=c('33475e')end
   end
   if y==25 and x%6==1 then v=c('718da3')end
   if y==25 and x%6==2 then v=c('a5b9c3')end
   if y==26 then v=c('33475e')end
   im:drawPixel(x,y,v)
  end
 end end
 -- Continuous raised ridge, within the existing opaque ridge silhouette.
 for x=19,44 do
  if blue[original:getPixel(x,14)] then im:drawPixel(x,14,c('a5b9c3'))end
  if blue[original:getPixel(x,15)] then im:drawPixel(x,15,c('718da3'))end
 end
 house:saveCopyAs(root..'/Prepared/tile-house-seed.png');house:close()
else
 local s=app.open(root..'/Prepared/cart.png');s.layers[1].name='Complete cart / aligned axle and paired shafts / A5'
 local cart=s.cels[1].image;local edge=Image(32,32,ColorMode.RGB)
 for y=0,31 do for x=0,31 do if pc.rgbaA(cart:getPixel(x,y))>0 then
  for _,q in ipairs({{1,0},{-1,0},{0,1},{0,-1}})do
   local xx,yy=x+q[1],y+q[2]
   if xx<0 or yy<0 or xx>=32 or yy>=32 or pc.rgbaA(cart:getPixel(xx,yy))==0 then edge:drawPixel(x,y,c('2e241d'));break end
  end
 end end end
 local layer=s:newLayer();layer.name='Native one pixel exterior contour';s:newCel(layer,1,edge,Point(0,0))
 -- A 2px sampled shaft must not become solid ink after the contour pass.
 -- Author paired 3px shafts from the same front rail; retain a lit wood center.
 local shafts=Image(32,32,ColorMode.RGB)
 for y=19,29 do
  local left=y<24 and 10 or 9;local right=32-left
  for _,cx in ipairs({left,right})do
   for x=cx-1,cx+1 do shafts:drawPixel(x,y,c('2e241d'))end
   if y<29 then shafts:drawPixel(cx,y,c('bc8850'))end
  end
 end
 local handles=s:newLayer();handles.name='Paired shafts with visible wood centers at native size';s:newCel(handles,1,shafts,Point(0,0))

 export(s,'cart')
 local approved=app.open(base..'/ArtReview/Approved/C7-Buildings-v5/tile-house.png');approved.layers[1].name='Approved whole house, preserved silhouette and facade'
 local candidate=app.open(root..'/Prepared/tile-house.png');local im=candidate.cels[1].image;local orig=approved.cels[1].image;local delta=Image(64,48,ColorMode.RGB)
 for y=0,47 do for x=0,63 do if im:getPixel(x,y)~=orig:getPixel(x,y)then delta:drawPixel(x,y,im:getPixel(x,y))end end end
 local layer=approved:newLayer();layer.name='Native roof ridge, tile courses and eave ends';approved:newCel(layer,1,delta,Point(0,0));candidate:close();export(approved,'tile-house')
end
