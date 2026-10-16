class Gobuster < Formula
  desc "Directory/file & DNS busting tool written in Go"
  homepage "https://github.com/OJ/gobuster"
  url "https://github.com/OJ/gobuster/releases/download/v3.8.2/gobuster_Windows_x86_64.zip"
  sha256 "677abe8e56c5455804225ad2264dc6e9981e99673ef3ccd6a2fa2af8a2e92aba"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/OJ/gobuster/archive/refs/tags/v3.8.2.tar.gz"
  end

  def install
    bin.install "gobuster.exe"
  end

  test do
    (testpath/"words.txt").write <<~EOS
      dog
      cat
      horse
      snake
      ape
    EOS

    output = shell_output("#{bin}/gobuster dir -u https://buffered.io -w words.txt 2>&1")
    assert_match "Finished", output

    assert_match version.major_minor.to_s, shell_output("#{bin}/gobuster --version")
  end
end
