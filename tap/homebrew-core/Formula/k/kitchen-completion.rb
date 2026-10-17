class KitchenCompletion < Formula
  desc "Bash completion for Kitchen"
  homepage "https://github.com/MarkBorcherding/test-kitchen-bash-completion"
  url "https://github.com/MarkBorcherding/test-kitchen-bash-completion/archive/refs/tags/v1.0.0.tar.gz"
  sha256 "6a9789359dab220df0afad25385dd3959012cfa6433c8c96e4970010b8cfc483"
  license "MIT"
  head "https://github.com/MarkBorcherding/test-kitchen-bash-completion.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "37e200065905b4bef71a4e59b42eafc6c61444de93119997633f5f20b21dcffa"
  end

  def install
    bash_completion.install "kitchen-completion.bash" => "kitchen"
  end

  test do
    assert_match "-F __kitchen_options",
      shell_output("bash -c 'source #{bash_completion}/kitchen && complete -p kitchen'")
  end
end
