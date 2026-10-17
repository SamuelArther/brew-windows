class Rmate < Formula
  desc "Edit files from an SSH session in TextMate"
  homepage "https://github.com/textmate/rmate"
  url "https://github.com/textmate/rmate/archive/refs/tags/v1.5.8.tar.gz"
  sha256 "40be07ae251bfa47b408eb56395dd2385d8e9ea220a19efd5145593cd8cbd89c"
  license "MIT"
  head "https://github.com/textmate/rmate.git", branch: "master"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/rmate-1.5.8"
    sha256 cellar: :any, x86_64_windows: "efc5b494e5d1604c4063288ad538192f102301f53606e7f091bdafc01c1c872c"
  end

  uses_from_macos "ruby"

  def install
    bin.install "bin/rmate"
  end

  test do
    system bin/"rmate", "--version"
  end
end
