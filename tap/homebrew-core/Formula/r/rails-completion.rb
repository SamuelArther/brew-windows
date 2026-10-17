class RailsCompletion < Formula
  desc "Bash completion for Rails"
  homepage "https://github.com/mernen/completion-ruby"
  url "https://github.com/mernen/completion-ruby/archive/refs/tags/v1.0.3.tar.gz"
  sha256 "cbcd002bba2a43730cff54f5386565917913d9dec16dcd89345fbe298fe4316b"
  license "MIT"
  version_scheme 1
  head "https://github.com/mernen/completion-ruby.git", branch: "main"

  livecheck do
    formula "ruby-completion"
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "e84261ebb42f2a6079df8bb5c46079be4e6ffec7c80d3e3f9a9a08ea6fa941b7"
  end

  def install
    bash_completion.install "completion-rails" => "rails"
  end

  test do
    assert_match "-F __rails",
      shell_output("bash -c 'source #{bash_completion}/rails && complete -p rails'")
  end
end
