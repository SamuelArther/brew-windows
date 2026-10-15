class Mqttui < Formula
  desc "Subscribe to a MQTT Topic or publish something quickly from the terminal"
  homepage "https://github.com/EdJoPaTo/mqttui"
  url "https://github.com/EdJoPaTo/mqttui/releases/download/v0.24.0/mqttui-v0.24.0-x86_64-pc-windows-msvc.zip"
  sha256 "57b19fb37879d508cbc389a5bf33b0d5e6f6031b5e8844066ee39c2313fb26f3"
  license "GPL-3.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/EdJoPaTo/mqttui/archive/refs/tags/v0.24.0.tar.gz"
  end

  def install
    bin.install "mqttui.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/mqttui --version 2>&1")
  end
end
