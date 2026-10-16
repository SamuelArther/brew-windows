class Droast < Formula
  desc "Opinionated Dockerfile linter"
  homepage "https://ewry.net/droast-dockerfile-linter/"
  url "https://github.com/immanuwell/dockerfile-roast/releases/download/1.7.0/droast-windows-x86_64.exe"
  sha256 "d4bf219ddc588e055fcaad10547a967d683fa7536b78cc96cc6e57ea75ff48c5"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/immanuwell/dockerfile-roast/archive/refs/tags/1.7.0.tar.gz"
  end

  def install
    bin.install "droast-windows-x86_64.exe" => "droast.exe"
  end

  test do
    (testpath/"Dockerfile").write <<~DOCKERFILE
      FROM alpine:3
      ENTRYPOINT ["echo", "hi"]
      ENTRYPOINT ["echo", "bye"]
    DOCKERFILE
    output = shell_output("#{bin}/droast --no-roast --format compact #{testpath}/Dockerfile", 1)
    assert_match "DF039", output
  end
end
