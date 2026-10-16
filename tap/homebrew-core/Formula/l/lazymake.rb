class Lazymake < Formula
  desc "Modern TUI for Makefiles"
  homepage "https://lazymake.vercel.app/"
  url "https://github.com/rshelekhov/lazymake/releases/download/v0.4.1/lazymake_0.4.1_Windows_x86_64.zip"
  sha256 "58a92930257e37f2fe55165c68996647f1b98a99b2c4cd9639e939f66a45a648"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/rshelekhov/lazymake/archive/refs/tags/v0.4.1.tar.gz"
  end

  def install
    bin.install "lazymake.exe"
  end

  test do
    # lazymake is a TUI, to verify the command is working we check the help output
    assert_match "Usage:", shell_output("#{bin}/lazymake --help")
  end
end
