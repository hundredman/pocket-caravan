-- Small native source edit: preserve complete editable objects and previous trials.
local root=assert(app.params.root);local base=assert(app.params.base);local pc=app.pixelColor
local function c(h)return pc.rgba(tonumber(h:sub(1,2),16),tonumber(h:sub(3,4),16),tonumber(h:sub(5,6),16),255)end
local cart=app.open(base..'/ArtReview/C7-Batch4-20261027/Native/cart.aseprite')
cart:saveAs(root..'/Native/cart.aseprite');cart:saveCopyAs(root..'/Native/cart.png');cart:close()
local house=app.open(base..'/ArtReview/C7-CartRoof-20261027/Native/tile-house.aseprite')
local flat=Image(house.spec);flat:drawSprite(house,1)
local delta=Image(64,48,ColorMode.RGB)
for y=10,15 do for x=0,63 do
 local v=flat:getPixel(x,y);local next=v
 if v==c('a5b9c3')then next=c('4a627a')
 elseif v==c('718da3')then next=c('4a627a')end
 -- Keep a narrow lit upper shoulder on the long ridge, blue rather than white.
 if y==14 and x>=19 and x<=44 and v==c('a5b9c3')then next=c('718da3')end
 if next~=v then delta:drawPixel(x,y,next)end
end end
local layer=house:newLayer();layer.name='Blue gray ridge matched to approved tiled roofs';house:newCel(layer,1,delta,Point(0,0))
house:saveAs(root..'/Native/tile-house.aseprite');house:saveCopyAs(root..'/Native/tile-house.png');house:close()
