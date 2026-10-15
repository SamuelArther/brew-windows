class Gh < Formula
  desc "GitHub command-line tool"
  homepage "https://cli.github.com/"
  url "https://github.com/cli/cli/releases/download/v2.102.0/gh_2.102.0_windows_amd64.zip"
  sha256 "ae64e556ecc240b200f7eba60d550e4bb60d78e860e69dd88c449405b86067f4"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/bin/gh.exe", "gh"
  end

  test do
    assert_match "gh version #{version}", shell_output("#{bin}/gh --version")
  end
end
