class Typical < Formula
  desc "Data interchange with algebraic data types"
  homepage "https://github.com/stepchowfun/typical"
  url "https://github.com/stepchowfun/typical/releases/download/v0.16.0/typical-x86_64-pc-windows-msvc.exe"
  sha256 "505c003c64381a38649e96631d4080f9ecfa5062ec3eceabc41aab109faef11b"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/stepchowfun/typical/archive/refs/tags/v0.16.0.tar.gz"
  end

  def install
    bin.install "typical-x86_64-pc-windows-msvc.exe" => "typical.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/typical --version 2>&1")
  end
end
