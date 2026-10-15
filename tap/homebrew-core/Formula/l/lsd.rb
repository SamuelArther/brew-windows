class Lsd < Formula
  desc "Clone of ls with colorful output, file type icons, and more"
  homepage "https://github.com/lsd-rs/lsd"
  url "https://github.com/lsd-rs/lsd/releases/download/v1.2.0/lsd-v1.2.0-x86_64-pc-windows-msvc.zip"
  sha256 "a65b9f457153934a7ce1f7e0fe1c4922a95bb2822222126e4fbaa6ee9c0d78e5"
  license "Apache-2.0"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "lsd.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/lsd --version")
  end
end
