class Binocle < Formula
  desc "Graphical tool to visualize binary data"
  homepage "https://github.com/sharkdp/binocle"
  url "https://github.com/sharkdp/binocle/releases/download/v0.3.2/binocle-v0.3.2-x86_64-pc-windows-msvc.zip"
  sha256 "9e7394fddf2b151b29150e3dfda26f824676ff43c20bce21ee63ec4b612a7ac1"
  license any_of: ["Apache-2.0", "MIT"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/sharkdp/binocle/archive/refs/tags/v0.3.2.tar.gz"
  end

  def install
    bin.install "binocle.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/binocle --version 2>&1")
  end
end
