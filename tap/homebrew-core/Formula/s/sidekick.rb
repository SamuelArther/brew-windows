class Sidekick < Formula
  desc "Deploy applications to your VPS"
  homepage "https://github.com/MightyMoud/sidekick"
  url "https://github.com/MightyMoud/sidekick/releases/download/v0.6.6/sidekick_Windows_x86_64.zip"
  sha256 "d527031b92bcc28b954a63287758cb4d2f1caa18c164224a5e4ea70a29b6fb42"
  license "GPL-3.0-only"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/MightyMoud/sidekick/archive/refs/tags/v0.6.6.tar.gz"
  end

  def install
    bin.install "sidekick.exe"
  end

  test do
    assert_match "With sidekick you can deploy any number of applications to a single VPS",
                  shell_output(bin/"sidekick")
    assert_match("Sidekick config not found - Run sidekick init", shell_output("#{bin}/sidekick deploy", 1))
  end
end
