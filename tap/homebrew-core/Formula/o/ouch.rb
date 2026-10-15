class Ouch < Formula
  desc "Painless compression and decompression for your terminal"
  homepage "https://github.com/ouch-org/ouch"
  url "https://github.com/ouch-org/ouch/releases/download/0.8.3/ouch-x86_64-pc-windows-msvc.zip"
  sha256 "6383a95987894412d5c9c4172e3d1d87c86aa322acb2b7ffcad42f492abc5dfe"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "ouch.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/ouch --version")
  end
end
