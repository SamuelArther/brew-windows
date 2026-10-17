class RubyCompletion < Formula
  desc "Bash completion for Ruby"
  homepage "https://github.com/mernen/completion-ruby"
  url "https://github.com/mernen/completion-ruby/archive/refs/tags/v1.0.3.tar.gz"
  sha256 "cbcd002bba2a43730cff54f5386565917913d9dec16dcd89345fbe298fe4316b"
  license "MIT"
  version_scheme 1
  head "https://github.com/mernen/completion-ruby.git", branch: "main"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/ruby-completion-1.0.3"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "f60375ce6769a6cf84612e2242432a2af644cfcaa7e97b1611394ccaed7bbc89"
  end

  def install
    bash_completion.install "completion-ruby" => "ruby"
  end

  test do
    assert_match "-F __ruby",
      shell_output("bash -c 'source #{bash_completion}/ruby && complete -p ruby'")
  end
end
