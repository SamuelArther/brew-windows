class Templ < Formula
  desc "Language for writing HTML user interfaces in Go"
  homepage "https://templ.guide"
  url "https://github.com/a-h/templ/releases/download/v0.3.1070/templ_Windows_x86_64.tar.gz"
  sha256 "6b3da83a851ae87ed0edfad21421f78ab921e5f818a07ac95f30d941a30cc219"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/a-h/templ/archive/refs/tags/v0.3.1070.tar.gz"
  end

  def install
    bin.install "templ.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/templ version")

    (testpath/"test.templ").write <<~TEMPL
      package main

      templ Test() {
        <p class="testing">Hello, World</p>
      }
    TEMPL

    output = shell_output("#{bin}/templ generate -stdout -f #{testpath}/test.templ")
    assert_match "func Test() templ.Component {", output
  end
end
