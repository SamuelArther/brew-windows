class Rojo < Formula
  desc "Professional grade Roblox development tools"
  homepage "https://rojo.space/"
  url "https://github.com/rojo-rbx/rojo/releases/download/v7.7.1/rojo-7.7.1-windows-x86_64.zip"
  sha256 "d4dfa3493a1613a43fc87312d6dac660d7c8bf38e6a99aa5dcd0d26154ba0b82"
  license "MPL-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/rojo-rbx/rojo.git"
  end

  def install
    bin.install "rojo.exe"
  end

  test do
    system bin/"rojo", "init"
    assert_path_exists testpath/"default.project.json"

    assert_match version.to_s, shell_output("#{bin}/rojo --version")
  end
end
