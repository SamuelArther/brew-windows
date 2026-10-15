class Age < Formula
  desc "Simple, modern, secure file encryption"
  homepage "https://github.com/FiloSottile/age"
  url "https://github.com/FiloSottile/age/releases/download/v1.3.2/age-v1.3.2-windows-amd64.zip"
  sha256 "f48d8f8f9ebe903ab5027ed067652f2cc1db94bc206976430133b905dcd8e8c7"
  license "BSD-3-Clause"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/age.exe", "age"
    windows_exe "libexec/age-keygen.exe", "age-keygen"
    windows_exe "libexec/age-inspect.exe", "age-inspect"
    windows_exe "libexec/age-plugin-batchpass.exe", "age-plugin-batchpass"
    windows_exe "libexec/age-plugin-pq.exe", "age-plugin-pq"
    windows_exe "libexec/age-plugin-tag.exe", "age-plugin-tag"
    windows_exe "libexec/age-plugin-tagpq.exe", "age-plugin-tagpq"
  end

  test do
    system bin/"age-keygen", "-o", "key.txt"
    pipe_output("#{bin}/age -e -i key.txt -o test.age", "test")
    assert_equal "test", shell_output("#{bin}/age -d -i key.txt test.age")
  end
end
