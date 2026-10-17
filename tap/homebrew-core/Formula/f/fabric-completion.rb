class FabricCompletion < Formula
  desc "Bash completion for Fabric"
  homepage "https://github.com/n0740/fabric-completion"
  url "https://github.com/n0740/fabric-completion/archive/5b5910492046e6335af0e88550176d2583d9a510.tar.gz"
  version "1"
  sha256 "34db5a8b207a66170580fc5c9d7521e76f3c3ee85471fa19a27718dca9a934a7"
  license "MIT"
  head "https://github.com/n0740/fabric-completion.git", branch: "master"

  livecheck do
    skip "No version information available to check"
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/fabric-completion-1"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "a99da222cbd03f49813d62674645adb9aee2af435b94e8ee35b9c1d5de14187f"
  end

  def install
    bash_completion.install "fabric-completion.bash" => "fabric"
  end

  def caveats
    <<~EOS
      All available tasks are cached in special file to speed up the response.
      Therefore, Add .fab_tasks~ to your ".gitignore".

      For more details and configuration refer to the home page.
    EOS
  end

  test do
    assert_match "-F __fab_completion",
      shell_output("bash -c 'source #{bash_completion}/fabric && complete -p fab'")
  end
end
