class LaunchctlCompletion < Formula
  desc "Bash completion for Launchctl"
  homepage "https://github.com/CamJN/launchctl-completion"
  url "https://github.com/CamJN/launchctl-completion/archive/refs/tags/v2.0.tar.gz"
  sha256 "990499494562cedf361482c5bf9f89b3a8b6a8aee92fda4e1eadaacb96688ecd"
  license "MIT"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/launchctl-completion-2.0"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "7443d259a712579712112a1f29d89dd94e63f995136e2b0f3e55f1435866250e"
  end

  def install
    bash_completion.install "launchctl-completion.sh" => "launchctl"
  end

  test do
    assert_match "-F _launchctl",
                 shell_output("bash -c 'source #{bash_completion}/launchctl && complete -p launchctl'")
  end
end
