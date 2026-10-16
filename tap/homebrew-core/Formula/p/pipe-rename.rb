class PipeRename < Formula
  desc "Rename your files using your favorite text editor"
  homepage "https://github.com/marcusbuffett/pipe-rename"
  url "https://github.com/marcusbuffett/pipe-rename/releases/download/1.6.7/renamer-1.6.7-x86_64-pc-windows-msvc.exe"
  sha256 "cd0769a503c129711e38787ebc38200b2f2c487c90dccc0e4159934e3d5e2bfd"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/marcusbuffett/pipe-rename/archive/refs/tags/1.6.7.tar.gz"
  end

  def install
    bin.install "renamer-1.6.7-x86_64-pc-windows-msvc.exe" => "renamer.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/renamer --version 2>&1")
  end
end
