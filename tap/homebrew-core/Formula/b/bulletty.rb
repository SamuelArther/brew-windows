class Bulletty < Formula
  desc "Pretty feed reader (ATOM/RSS) that stores articles in Markdown files"
  homepage "https://bulletty.croci.dev/"
  url "https://github.com/crocidb/bulletty/releases/download/v0.3.0/bulletty-x86_64-pc-windows-msvc.zip"
  sha256 "3f281dd650a29cebebce54e9deae35cdecc96109edecfe5bcc10791729fd4e05"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/CrociDB/bulletty/archive/refs/tags/v0.3.0.tar.gz"
  end

  def install
    bin.install "bulletty.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/bulletty --version")
    assert_match "Feeds Registered", shell_output("#{bin}/bulletty list")
  end
end
