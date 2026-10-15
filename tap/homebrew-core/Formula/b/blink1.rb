class Blink1 < Formula
  desc "Control blink(1) indicator light"
  homepage "https://blink1.thingm.com/"
  url "https://github.com/todbot/blink1-tool/releases/download/v2.5.0/blink1-tool-v2.5.0-windows-x86_64.zip"
  sha256 "997fa0a19f5e7bd157d8bca3b19c57a93b005fa77c74b3684b52627d244cd25f"
  license any_of: ["CC-BY-SA-4.0", "MIT"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/todbot/blink1-tool.git"
  end

  def install
    bin.install "blink1-tool.exe"
  end

  test do
    system bin/"blink1-tool", "--version"
  end
end
