class Feroxbuster < Formula
  desc "Fast, simple, recursive content discovery tool written in Rust"
  homepage "https://epi052.github.io/feroxbuster"
  url "https://github.com/epi052/feroxbuster/releases/download/v2.13.1/x86_64-windows-feroxbuster.exe.zip"
  sha256 "6255fc1998220833d22b221e5c740895ada2a5fbaf31587e799276c59da7dab2"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/epi052/feroxbuster/archive/refs/tags/v2.13.1.tar.gz"
  end

  def install
    bin.install "feroxbuster.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/feroxbuster --version 2>&1")
  end
end
