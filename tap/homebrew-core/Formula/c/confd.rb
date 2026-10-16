class Confd < Formula
  desc "Manage local application configuration files using templates"
  homepage "https://github.com/kelseyhightower/confd"
  url "https://github.com/kelseyhightower/confd/releases/download/v0.16.0/confd-0.16.0-windows-amd64.exe"
  sha256 "81e561145cc0eea5da99be27642a03493e926d73e97ea88bf14874f03bcbfae9"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/kelseyhightower/confd/archive/refs/tags/v0.16.0.tar.gz"
  end

  def install
    bin.install "confd-0.16.0-windows-amd64.exe" => "confd.exe"
  end

  test do
    templatefile = testpath/"templates/test.tmpl"
    templatefile.write <<~EOS
      version = {{getv "/version"}}
    EOS

    conffile = testpath/"conf.d/conf.toml"
    conffile.write <<~TOML
      [template]
      prefix = "/"
      src = "test.tmpl"
      dest = "./test.conf"
      keys = [
          "/version"
      ]
    TOML

    keysfile = testpath/"keys.yaml"
    keysfile.write <<~YAML
      version: v1
    YAML

    system bin/"confd", "-backend", "file", "-file", "keys.yaml", "-onetime", "-confdir=."
    assert_path_exists testpath/"test.conf"
    refute_predicate (testpath/"test.conf").size, :zero?
  end
end
