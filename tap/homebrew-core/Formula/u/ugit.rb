class Ugit < Formula
  desc "Undo git commands. Your damage control git buddy"
  homepage "https://bhupesh.me/undo-your-last-git-mistake-with-ugit/"
  url "https://github.com/Bhupesh-V/ugit/archive/refs/tags/v5.9.tar.gz"
  sha256 "f93d9d4bb0d6fd676704e45733190413885c859ff2807b84cc8113bf674fc063"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "c53a9d5f304b0457ed03983c068bc84f42b228feaeba9becaec93008f3239d4c"
  end

  depends_on "fzf"

  on_macos do
    depends_on "bash"
  end

  conflicts_with "git-extras", because: "both install `git-undo` binaries"

  def install
    bin.install "ugit"
    bin.install "git-undo"
  end

  test do
    assert_match "ugit version #{version}", shell_output("#{bin}/ugit --version")
    assert_match "Ummm, you are not inside a Git repo", shell_output(bin/"ugit")
  end
end
