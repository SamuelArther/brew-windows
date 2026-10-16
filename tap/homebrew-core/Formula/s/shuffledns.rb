class Shuffledns < Formula
  desc "Enumerate subdomains using active bruteforce & resolve subdomains with wildcards"
  homepage "https://projectdiscovery.io"
  url "https://github.com/projectdiscovery/shuffledns/releases/download/v1.2.1/shuffledns_1.2.1_windows_amd64.zip"
  sha256 "8d7d9aad6d2c5ea33282e6abc8d26d7aa566b1ede84c194337d761d746d121f6"
  license "GPL-3.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/projectdiscovery/shuffledns/archive/refs/tags/v1.2.1.tar.gz"
  end

  def install
    bin.install "shuffledns.exe"
  end

  test do
    assert_match "resolver file doesn't exists", shell_output("#{bin}/shuffledns 2>&1", 1)
    assert_match version.to_s, shell_output("#{bin}/shuffledns -version 2>&1")
  end
end
