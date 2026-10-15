class Lolcrab < Formula
  desc "Make your console colorful, with OpenSimplex noise"
  homepage "https://github.com/mazznoer/lolcrab"
  url "https://github.com/mazznoer/lolcrab/releases/download/v0.4.1/lolcrab-x86_64-pc-windows-msvc.zip"
  sha256 "64e9a6d764aa33fb7c94d37c5695f4e92c696d7645464b5ef0a71caf0c756731"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/mazznoer/lolcrab/archive/refs/tags/v0.4.1.tar.gz"
  end

  def install
    bin.install "lolcrab.exe"
  end

  test do
    assert_match "\e[38;", pipe_output(bin/"lolcrab", "lorem ipsum dolor sit amet")
    assert_match "\e[48;", pipe_output("#{bin}/lolcrab -i", "lorem ipsum dolor sit amet")
  end
end
