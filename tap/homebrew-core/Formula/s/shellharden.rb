class Shellharden < Formula
  desc "Bash syntax highlighter that encourages/fixes variables quoting"
  homepage "https://github.com/anordal/shellharden"
  url "https://github.com/anordal/shellharden/releases/download/v4.3.2/shellharden-x86_64-pc-windows-msvc.zip"
  sha256 "cf08b4d13b1a3b34046d83ea8cf13b4fcf8a99b103a83d74283921c9c45a2e9f"
  license "MPL-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/anordal/shellharden/archive/refs/tags/v4.3.2.tar.gz"
  end

  def install
    bin.install "shellharden.exe"
  end

  test do
    (testpath/"script.sh").write <<~SH
      dog="poodle"
      echo $dog
    SH
    system bin/"shellharden", "--replace", "script.sh"
    assert_match "echo \"$dog\"", (testpath/"script.sh").read
  end
end
