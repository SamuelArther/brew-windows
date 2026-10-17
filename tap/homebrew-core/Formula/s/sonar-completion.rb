class SonarCompletion < Formula
  desc "Bash completion for Sonar"
  homepage "https://github.com/a1dutch/sonarqube-bash-completion"
  url "https://github.com/a1dutch/sonarqube-bash-completion/archive/refs/tags/1.1.tar.gz"
  sha256 "506a592b166cff88786ae9e6215f922b8ed3617c65a4a88169211a80ef1c6b66"
  license "Apache-2.0"
  head "https://github.com/a1dutch/sonarqube-bash-completion.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "9b32e1a0df4853ab47ba7b0082e2f23023759b0d764ac0d6d8c94f8a0514793e"
  end

  def install
    bash_completion.install "etc/bash_completion.d/sonar"
  end

  test do
    assert_match "-F _sonar",
      shell_output("bash -c 'source #{bash_completion}/sonar && complete -p sonar'")
  end
end
