class Viu < Formula
  desc "Simple terminal image viewer written in Rust"
  homepage "https://github.com/atanunq/viu"
  url "https://github.com/atanunq/viu/releases/download/v1.6.1/viu-x86_64-pc-windows-msvc.exe"
  sha256 "f2d16bbe088b205985a49d4f7e4860c74818d41a93a294ba1cb67d4879d55b43"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/atanunq/viu/archive/refs/tags/v1.6.1.tar.gz"
  end

  def install
    bin.install "viu-x86_64-pc-windows-msvc.exe" => "viu.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/viu --version 2>&1")
  end
end
