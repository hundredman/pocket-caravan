-- Targeted native edit of the existing A4 design. Keep its three colors;
-- replace the cross-like silhouette with one smooth ellipse, no side spurs.
local root=assert(app.params.root)
local s=Sprite(80,48,ColorMode.RGB);s.layers[1].name='Rounded natural pond / existing A4 colors'
local im=s.cels[1].image;local pc=app.pixelColor
local colors={pc.rgba(132,103,94,255),pc.rgba(120,160,153,255),pc.rgba(127,185,175,255)}
local function inside(x,y)
 return ((x-39.5)/34.5)^2+((y-23.5)/20)^2<=1
end
for y=0,47 do for x=0,79 do
 if inside(x,y) then
  local d=4
  for dy=-3,3 do for dx=-3,3 do
   local n=math.abs(dx)+math.abs(dy)
   if n>0 and n<=3 and not inside(x+dx,y+dy) then d=math.min(d,n) end
  end end
  im:drawPixel(x,y,colors[d==1 and 1 or d==2 and 2 or 3])
 end
end end
s:saveAs(root..'/pond.aseprite');s:saveCopyAs(root..'/pond-source.png');s:close()
print('80x48 native rounded pond, original A4 water/bank colors, no protruding caps')
