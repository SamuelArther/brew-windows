class TmuxinatorCompletion < Formula
  desc "Shell completion for Tmuxinator"
  homepage "https://github.com/tmuxinator/tmuxinator"
  url "https://github.com/tmuxinator/tmuxinator/archive/refs/tags/v3.4.2.tar.gz"
  sha256 "5abf32ddd6cb22fac7991310024bc14d95820e09cb7d86c6b633210afd549f67"
  license "MIT"
  head "https://github.com/tmuxinator/tmuxinator.git", branch: "master"

  livecheck do
    formula "tmuxinator"
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/tmuxinator-completion-3.4.2"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "8fc77ee3586f99675954885f5835dbf80e59cb8edfce2564e2ee4054e8120f14"
  end

  def install
    bash_completion.install "completion/tmuxinator.bash" => "tmuxinator"
    zsh_completion.install "completion/tmuxinator.zsh" => "_tmuxinator"
    fish_completion.install Dir["completion/*.fish"]
  end

  test do
    assert_match "-F _tmuxinator",
      shell_output("bash -c 'source #{bash_completion}/tmuxinator && complete -p tmuxinator'")
  end
end
