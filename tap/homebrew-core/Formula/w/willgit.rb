class Willgit < Formula
  desc "William's miscellaneous git tools"
  homepage "https://github.com/DanielVartanov/willgit"
  url "https://github.com/DanielVartanov/willgit/archive/refs/tags/1.0.0.tar.gz"
  sha256 "3bb99d6ec2614a90f40962311daf51f393b3d0abfdb0f9e0a14ba7340b33a2c8"
  license "MIT"
  head "https://github.com/DanielVartanov/willgit.git", branch: "master"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/willgit-1.0.0"
    sha256 cellar: :any, x86_64_windows: "d093deddbd8141431f9be795b2b19b99ad666d48e79650644fceee299e29ef1a"
  end

  uses_from_macos "ruby"

  def install
    prefix.install "bin"
  end

  test do
    system "git", "init", "--initial-branch=main"
    (testpath/"README.md").write "# BrewTest"
    system "git", "add", "README.md"
    system "git", "commit", "-m", "init"
    assert_equal "Local branch: main", shell_output("git wtf").chomp
  end
end
