class Grizzly < Formula
  desc "Command-line tool for managing and automating Grafana dashboards"
  homepage "https://grafana.github.io/grizzly/"
  url "https://github.com/grafana-cold-storage/grizzly/archive/refs/tags/v0.7.1.tar.gz"
  sha256 "81811b684ef1bddd3b7147c5095224552a0b35dc3ff210d10e6cbc5e12331160"
  license "Apache-2.0"
  head "https://github.com/grafana-cold-storage/grizzly.git", branch: "main"

  # https://github.com/grafana-cold-storage/grizzly/pull/613
  deprecate! date: "2026-02-17", because: :repo_archived, replacement_formula: "grafanactl"
  disable! date: "2027-02-17", because: :repo_archived, replacement_formula: "grafanactl"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "99ee5ac2b2f30d923858a8af1cab677c64b8f05026e9dac482e45829690247e8"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = "-X github.com/grafana/grizzly/pkg/config.Version=#{version}"
    system "go", "build", *std_go_args(ldflags:, output: bin/"grr"), "./cmd/grr"
  end

  test do
    sample_dashboard = testpath/"dashboard_simple.yaml"
    sample_dashboard.write <<~YAML
      apiVersion: grizzly.grafana.com/v1alpha1
      kind: Dashboard
      metadata:
        folder: sample
        name: prod-overview
      spec:
        schemaVersion: 17
        tags:
          - templated
        timezone: browser
        title: Production Overview
        uid: prod-overview
    YAML

    assert_match "prod-overview", shell_output("#{bin}/grr list #{sample_dashboard}")

    assert_match version.to_s, shell_output("#{bin}/grr --version 2>&1")
  end
end
