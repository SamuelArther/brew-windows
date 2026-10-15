class Par2 < Formula
  desc "Parchive: Parity Archive Volume Set for data recovery"
  homepage "https://parchive.github.io"
  url "https://github.com/Parchive/par2cmdline/releases/download/v1.4.0/par2cmdline-1.4.0-win-x64.zip"
  sha256 "200130143dba1e12f9d1be567d5a446e785d2093e5d735225d8ad0539125b71c"
  license "GPL-2.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/Parchive/par2cmdline/releases/download/v1.4.0/par2cmdline-1.4.0.tar.bz2"
    strategy :github_latest
  end

  def install
    bin.install "par2.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/par2 --version 2>&1")
  end
end
