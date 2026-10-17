class Shellspec < Formula
  desc "BDD unit testing framework for dash, bash, ksh, zsh and all POSIX shells"
  homepage "https://shellspec.info/"
  url "https://github.com/shellspec/shellspec/archive/refs/tags/0.28.1.tar.gz"
  sha256 "400d835466429a5fe6c77a62775a9173729d61dd43e05dfa893e8cf6cb511783"
  license "MIT"
  head "https://github.com/shellspec/shellspec.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "d32dbbc99832c83199829ee64fdb6d8311cf598669e3f7e3b0602a616e80c92d"
  end

  def install
    system "make", "install", "PREFIX=#{prefix}"
  end

  test do
    system bin/"shellspec", "--init"
    assert_match "--require spec_helper", (testpath/".shellspec").read
    assert_path_exists testpath/"spec/spec_helper.sh"

    (testpath/"spec/example_spec.sh").write <<~SPEC
      Describe 'hello.sh'
        Include lib/hello.sh
        It 'says hello'
          When call hello ShellSpec
          The output should equal 'Hello ShellSpec!'
        End
      End
    SPEC

    (testpath/"lib/hello.sh").write <<~SHELL
      hello() {
        echo "Hello ${1}!"
      }
    SHELL

    output = shell_output("#{bin}/shellspec --shell sh 2>&1", 102)
    assert_match "1 example, 0 failures", output

    assert_match version.to_s, shell_output("#{bin}/shellspec --version")
  end
end
