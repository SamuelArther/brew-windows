class SarifFmt < Formula
  desc "Pretty print SARIF files to easy human readable output"
  homepage "https://docs.rs/crate/sarif-fmt/latest"
  url "https://github.com/psastras/sarif-rs/archive/refs/tags/sarif-fmt-v0.8.0.tar.gz"
  sha256 "5182811d6ce671b9443fc6032028612fe0f60e8c37b177710e3edba0d7d2db88"
  license "MIT"
  head "https://github.com/psastras/sarif-rs.git", branch: "main"

  livecheck do
    url :stable
    regex(/^sarif-fmt[._-]v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "dce7c9a4a672851e9443a673841f1301ec0d613b65be0ba0dac2569889b999cc"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args(path: "sarif-fmt")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/sarif-fmt --version")

    (testpath/"test.cpp").write <<~CPP
      #include <stdlib.h>

      int string_to_int(const char *num) {
        return atoi(num);
      }
    CPP

    (testpath/"test.sarif").write <<~JSON
      {
        "runs": [
          {
            "results": [
              {
                "level": "warning",
                "locations": [
                  {
                    "physicalLocation": {
                      "artifactLocation": {
                        "uri": "test.cpp"
                      },
                      "region": {
                        "startColumn": 10,
                        "startLine": 4
                      }
                    }
                  }
                ],
                "message": {
                  "text": "'atoi' used to convert a string to an integer value, but function will not report conversion errors; consider using 'strtol' instead [cert-err34-c]"
                }
              }
            ],
            "tool": {
              "driver": {
                "name": "clang-tidy"
              }
            }
          }
        ],
        "version": "2.1.0"
      }
    JSON

    output = shell_output("#{bin}/sarif-fmt -i test.sarif")
    assert_match "warning: 'atoi' used to convert a string to an integer value", output
  end
end
