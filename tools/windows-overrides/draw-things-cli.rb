class DrawThingsCli < Formula
  desc "Local image generation CLI (stable-diffusion.cpp on Windows)"
  homepage "https://github.com/leejet/stable-diffusion.cpp"
  url "https://github.com/leejet/stable-diffusion.cpp/releases/download/master-968-8cd731e/sd-master-8cd731e-bin-win-cpu-x64.zip"
  version "968"
  sha256 "3289065032e298d751964ced2fb444c6e11f58793ba14c76f1f6a1d5b5ffff9c"
  license "MIT"

  # draw-things-cli is the Swift/Metal engine of the Draw Things app and builds only for Apple
  # platforms. stable-diffusion.cpp (official leejet CPU build, release master-968-8cd731e) is the
  # standard local Stable Diffusion / FLUX / Wan runtime on Windows. It loads model files
  # directly instead of Draw Things' model catalog, with its own options:
  #   draw-things-cli -m sd_xl_turbo_1.0.safetensors -p "a lighthouse at dusk" -o out.png

  livecheck do
    url :stable
    regex(/^master-(\d+)-\h+$/i)
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/sd-cli.exe", "draw-things-cli"
    windows_exe "libexec/sd-server.exe", "draw-things-server"
  end

  def caveats
    <<~EOS
      draw-things-cli is stable-diffusion.cpp's sd-cli: it takes a model file (.safetensors,
      .ckpt or .gguf, e.g. from Hugging Face) with -m, a prompt with -p and an output with -o.
      LoRA training is not part of stable-diffusion.cpp; LoRAs are applied with --lora-model-dir.
    EOS
  end

  test do
    assert_match "--prompt", shell_output("#{bin}/draw-things-cli --help 2>&1")
    output = shell_output("#{bin}/draw-things-cli -m #{testpath}/missing.safetensors -p test -o #{testpath}/out.png 2>&1", 1)
    assert_match "missing.safetensors", output
    refute_path_exists testpath/"out.png"
  end
end
