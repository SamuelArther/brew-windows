class Typst < Formula
  desc "Markup-based typesetting system"
  homepage "https://typst.app/"
  url "https://github.com/typst/typst/releases/download/v0.15.1/typst-x86_64-pc-windows-msvc.zip"
  sha256 "19ce3551153c2fe7ee9fa2f95208310c8f4d3209fedb699e0333faf8913f6736"
  license "Apache-2.0"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "typst.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/typst --version")
  end
end
