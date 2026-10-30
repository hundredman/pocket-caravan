-- Pack the authored native PNGs into an editable tagged 16-frame animation.
local dir=assert(app.params.frames)
local name=assert(app.params.name)
local output=assert(app.params.output)
local sprite=Sprite(16,24,ColorMode.RGB)
sprite.layers[1].name='native character'
local index=0
for d in string.gmatch('dulr','.') do
  for f=0,3 do
    index=index+1
    if index>1 then sprite:newEmptyFrame() end
    local image=Image{fromFile=app.fs.joinPath(dir,name..'_'..d..f..'.png')}
    if index==1 then sprite.cels[1].image=image else sprite:newCel(sprite.layers[1],index,image,Point(0,0)) end
    sprite.frames[index].duration=0.125
  end
end
-- Add tags only after all frames exist: inserting frames expands earlier tag ranges.
local start=1
for d in string.gmatch('dulr','.') do
  local tag=sprite:newTag(start,start+3)
  tag.name=d
  start=start+4
end
sprite:saveAs(output)
sprite:close()
print('16_FRAMES_TAGGED '..name)
