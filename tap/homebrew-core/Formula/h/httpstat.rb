class Httpstat < Formula
  include Language::Python::Shebang

  desc "Curl statistics made simple"
  homepage "https://github.com/reorx/httpstat"
  url "https://github.com/reorx/httpstat/archive/refs/tags/1.3.2.tar.gz"
  sha256 "56c45aebdb28160dd16c73cf23af8208c19b30ec0166790685dfec115df9c92f"
  license "MIT"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/httpstat-1.3.2"
    sha256 cellar: :any, x86_64_windows: "6dbc11ffdeb925d1f98cfa9a3f1f1bf75a3693c956558ba8910b597601c0eed0"
  end

  uses_from_macos "curl"
  uses_from_macos "python"

  def install
    rw_info = python_shebang_rewrite_info("/usr/bin/env python3")
    rewrite_shebang rw_info, "httpstat.py"
    bin.install "httpstat.py" => "httpstat"
  end

  test do
    assert_match "HTTP", shell_output("#{bin}/httpstat https://github.com")
  end
end
