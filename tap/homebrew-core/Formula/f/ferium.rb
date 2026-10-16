class Ferium < Formula
  desc "Fast and multi-source CLI program for managing Minecraft mods and modpacks"
  homepage "https://github.com/gorilla-devs/ferium"
  url "https://github.com/gorilla-devs/ferium/releases/download/v4.7.1/ferium-windows-gnu.zip"
  sha256 "90cb959521ceef4acfe7aef0b2a562c75cc0f9f0f5e5218191e2f0746feaedb1"
  license "MPL-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/gorilla-devs/ferium/archive/refs/tags/v4.7.1.tar.gz"
  end

  def install
    bin.install "ferium.exe"
  end

  test do
    system bin/"ferium", "--help"
    ENV["FERIUM_CONFIG_FILE"] = testpath/"config.json"
    system bin/"ferium", "profile", "create",
                         "--game-version", "1.19",
                         "--mod-loader", "quilt",
                         "--output-dir", testpath/"mods",
                         "--name", "test"
    system bin/"ferium", "add", "sodium"
    system bin/"ferium", "list", "--verbose"
    system bin/"ferium", "upgrade"
    refute_empty testpath.glob("mods/*.jar")
  end
end
