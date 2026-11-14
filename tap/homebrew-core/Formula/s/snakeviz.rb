class Snakeviz < Formula
  include Language::Python::Virtualenv

  desc "Web-based viewer for Python profiler output"
  homepage "https://jiffyclub.github.io/snakeviz/"
  url "https://files.pythonhosted.org/packages/04/06/82f56563b16d33c2586ac2615a3034a83a4ff1969b84c8d79339e5d07d73/snakeviz-2.2.2.tar.gz"
  sha256 "08028c6f8e34a032ff14757a38424770abb8662fb2818985aeea0d9bc13a7d83"
  license "BSD-3-Clause"
  revision 8

  bottle do
    sha256 cellar: :any, x86_64_windows: "e946ddcfc6bb3e35e5e54c10a763c75e9c2fba68aae724a7720e44035ee39056"
  end

  depends_on "python@3.15"

  resource "tornado" do
    url "https://files.pythonhosted.org/packages/64/2b/8ff2b41620cbba0eeb0862fed21dcaa5a2ad36c6dd9bb465f9ff7a147be2/tornado-6.6.1.tar.gz"
    sha256 "28e6311402af59c8b02e3fa0f5e7d8eb465b25081381803b127d97e6a4395d5a"
  end

  def install
    virtualenv_install_with_resources
  end

  test do
    require "cgi"
    system bin/"snakeviz", "--version"
    system python3, "-m", "cProfile", "-o", "output.prof", "-m", "cProfile"

    port = free_port

    output_file = testpath/"output.prof"

    pid = fork do
      exec bin/"snakeviz", "--port", port.to_s, "--server", output_file
    end
    sleep 3
    output = shell_output("curl -s http://localhost:#{port}/snakeviz/#{ERB::Util.url_encode output_file}")
    assert_match "cProfile", output
  ensure
    Process.kill("HUP", pid)
  end
end
