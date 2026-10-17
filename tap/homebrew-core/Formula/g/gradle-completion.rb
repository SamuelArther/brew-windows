class GradleCompletion < Formula
  desc "Bash and Zsh completion for Gradle"
  homepage "https://gradle.org/"
  url "https://github.com/gradle/gradle-completion/archive/refs/tags/v9.8.0.tar.gz"
  sha256 "15672384608402919caf0d7c51e3b6675c006724b099045d21a269e8cb0b006d"
  license "MIT"
  compatibility_version 1
  head "https://github.com/gradle/gradle-completion.git", branch: "master"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/gradle-completion-9.8.0"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "63a5be104a5f18111d7c1051b5fe5b563f75e59b691edff50adccce00e120815"
  end

  def install
    bash_completion.install "gradle-completion.bash" => "gradle"
    zsh_completion.install "_gradle"
  end

  test do
    assert_match "-F _gradle",
      shell_output("bash -c 'source #{bash_completion}/gradle && complete -p gradle'")
  end
end
