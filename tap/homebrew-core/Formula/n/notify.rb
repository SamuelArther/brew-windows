class Notify < Formula
  desc "Stream the output of any CLI and publish it to a variety of supported platforms"
  homepage "https://docs.projectdiscovery.io/tools/notify/overview"
  url "https://github.com/projectdiscovery/notify/releases/download/v1.0.7/notify_1.0.7_windows_amd64.zip"
  sha256 "1480b43f2e6c961c68fc3cbdf69772c168ca70049e849925d854c2cb4d96a570"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/projectdiscovery/notify/archive/refs/tags/v1.0.7.tar.gz"
  end

  def install
    bin.install "notify.exe"
  end

  test do
    assert_match "Current Version: #{version}", shell_output("#{bin}/notify -disable-update-check -version 2>&1")
    output = shell_output("#{bin}/notify -disable-update-check -config \"#{testpath}/non_existent\" 2>&1", 1)
    assert_match "Could not read config", output
  end
end
