class Docuum < Formula
  desc "Perform least recently used (LRU) eviction of Docker images"
  homepage "https://github.com/stepchowfun/docuum"
  url "https://github.com/stepchowfun/docuum/releases/download/v0.27.1/docuum-x86_64-pc-windows-msvc.exe"
  sha256 "e9c598d378a2f9bd8bf677af02ffc318e372ae9d31faac3de7b4b527fedfafb3"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/stepchowfun/docuum/archive/refs/tags/v0.27.1.tar.gz"
  end

  def install
    bin.install "docuum-x86_64-pc-windows-msvc.exe" => "docuum.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/docuum --version 2>&1")
  end
end
