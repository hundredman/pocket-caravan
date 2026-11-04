-- Whole-object offline preparation of fair-day sprites; no production asset edits.
local root=app.params.repo
local out=root..'/ArtReview/CentralFair-Task4-20261104/'
local pc=app.pixelColor
local reference=app.open(root..'/Assets/PocketCaravan/Art/Goryeo/Markets/grain_stall.png')
local palette,seen={},{}
for p in reference.cels[1].image:pixels() do local c=p();if pc.rgbaA(c)==255 and not seen[c]then palette[#palette+1]=c;seen[c]=true end end
reference:close()
for _,v in ipairs({{49,52,90},{77,80,136},{109,117,169},{39,83,75},{59,118,101},{100,155,124},{128,50,37},{178,72,45},{211,116,65}})do palette[#palette+1]=pc.rgba(v[1],v[2],v[3],255)end
local function nearest(c)
 local best,dist=nil,1e20
 for _,v in ipairs(palette)do local d=(pc.rgbaR(c)-pc.rgbaR(v))^2+(pc.rgbaG(c)-pc.rgbaG(v))^2+(pc.rgbaB(c)-pc.rgbaB(v))^2;if d<dist then best=v;dist=d end end
 return best
end
for _,name in ipairs({'fair_indigo','fair_teal'})do
 local s=app.open(out..'Raw/'..name..'.png');local im=s.cels[1].image
 local x0,y0,x1,y1=im.width,im.height,0,0
 for p in im:pixels()do if pc.rgbaA(p())>=224 then x0=math.min(x0,p.x);y0=math.min(y0,p.y);x1=math.max(x1,p.x);y1=math.max(y1,p.y)end end
 local bw,bh=x1-x0+1,y1-y0+1;local scale=math.min(46/bw,46/bh);local w,h=math.floor(bw*scale),math.floor(bh*scale)
 local dest=Sprite(48,48,ColorMode.RGB);local di=dest.cels[1].image;local ox,oy=math.floor((48-w)/2),47-h
 for y=0,h-1 do for x=0,w-1 do local c=im:getPixel(x0+math.min(bw-1,math.floor((x+.5)*bw/w)),y0+math.min(bh-1,math.floor((y+.5)*bh/h)));if pc.rgbaA(c)>=128 then di:drawPixel(ox+x,oy+y,nearest(c))end end end
 dest:saveAs(out..'Native/'..name..'.aseprite');dest:saveAs(out..'Native/'..name..'.png');dest:close();s:close()
end
