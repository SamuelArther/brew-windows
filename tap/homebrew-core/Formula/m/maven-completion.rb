class MavenCompletion < Formula
  desc "Bash completion for Maven"
  homepage "https://github.com/juven/maven-bash-completion"
  url "https://github.com/juven/maven-bash-completion/archive/refs/tags/20200420.tar.gz"
  sha256 "eb4ef412d140e19e7d3ce23adb7f8fcce566f44388cfdc8c1e766a3c4b183d3d"
  license "Apache-2.0"
  head "https://github.com/juven/maven-bash-completion.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "c4e258bb2102b1422c89efc05c18b64c2864b3ac5fccb60d3866a9db17de705c"
  end

  def install
    bash_completion.install "bash_completion.bash" => "maven"
  end

  test do
    assert_match "-F _mvn",
      shell_output("bash -c 'source #{bash_completion}/maven && complete -p mvn'")
  end
end
