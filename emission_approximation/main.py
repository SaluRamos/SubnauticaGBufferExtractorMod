import os

class ImageSet:

    albedo = None
    specular = None
    final_render = None
    no_light = None
    result = None

    def __init__(self, name:str):
        self.albedo = f"{name}_albedo.jpg"
        self.specular = f"{name}_specular.jpg"
        self.final_render = f"{name}_base.jpg"
        self.no_light = f"{name}_no_light.jpg"

def apply_shader(image:ImageSet):
    pass

if __name__ == "__main__":
    #load images from folder
    files = os.listdir("captures")

    unique_files = set()
    for file in files:
        parts = file.split("_")
        name = parts[0] + "_" + parts[1]
        unique_files.add(name)

    print(len(unique_files))
    print(unique_files)

    images = []
    for file in unique_files:
        images.append(ImageSet(file))
    #apply shader to images

    #save modified images
    os.makedirs("output", exist_ok=True)