class Difi < Formula
  desc "Pixel-perfect terminal diff viewer"
  homepage "https://github.com/xguot/difi"
  url "https://github.com/xguot/difi/releases/download/v0.2.31/difi_Windows_x86_64.zip"
  sha256 "d21cc85950c5c9dd9528c5fd1b9d320fa392f7fec8ef4c21308c947d078fdfea"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/xguot/difi/archive/refs/tags/v0.2.31.tar.gz"
  end

  def install
    bin.install "difi.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/difi -version")

    system "git", "init"
    system "git", "config", "user.email", "test@example.com"
    system "git", "config", "user.name", "Test"

    (testpath/"file.txt").write("one")
    system "git", "add", "file.txt"
    system "git", "commit", "-m", "init"

    File.write(testpath/"file.txt", "two")
    system "git", "commit", "-am", "change"

    output = shell_output("#{bin}/difi --plain HEAD~1")
    assert_match "file.txt", output
  end
end
