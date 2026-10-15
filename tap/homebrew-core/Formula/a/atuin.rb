class Atuin < Formula
  desc "Improved shell history for zsh, bash, fish and nushell"
  homepage "https://atuin.sh/"
  url "https://github.com/atuinsh/atuin/releases/download/v18.23.0/atuin-x86_64-pc-windows-msvc.zip"
  sha256 "3e1c1282da39039a7ceb8e53c6b624dfb86200823559bbe80c0ace3d0baf09ae"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "atuin.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/atuin --version")
  end
end
