class SpringCompletion < Formula
  desc "Bash completion for Spring"
  homepage "https://github.com/jacaetevha/spring_bash_completion"
  url "https://github.com/jacaetevha/spring_bash_completion/archive/refs/tags/v0.0.1.tar.gz"
  sha256 "a97b256dbdaca894dfa22bd96a6705ebf4f94fa8206d05f41927f062c3dd60bf"
  license "Unlicense"
  head "https://github.com/jacaetevha/spring_bash_completion.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "2355908e4973ac2536a39bf69e49a8dd1e75d1ad318340d401426750afac5293"
  end

  def install
    bash_completion.install "spring.bash" => "spring"
  end

  test do
    assert_match "-F _spring",
      shell_output("bash -c 'source #{bash_completion}/spring && complete -p spring'")
  end
end
