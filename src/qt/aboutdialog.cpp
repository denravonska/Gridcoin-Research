#include "aboutdialog.h"
#include "ui_aboutdialog.h"
#include "clientmodel.h"

#include "global_objects_noui.hpp"

AboutDialog::AboutDialog(QWidget *parent) :
    QDialog(parent),
    ui(new Ui::AboutDialog)
{
    ui->setupUi(this);

    // Set current copyright year and boinc utilization
    ui->copyrightLabel->setText(
                tr("Boinc Magnitude: ") + QString::number(nBoincUtilization) + "              , " +
                tr("Registered Version: ") + QString::fromUtf8(sRegVer.c_str()) + "             " +
                tr("Copyright 2009-2018 The Bitcoin/Peercoin/Black-Coin/Gridcoin developers"));
}

void AboutDialog::setModel(ClientModel *model)
{
    if(model)
    {
        ui->versionLabel->setText(model->formatFullVersion());
    }
}

AboutDialog::~AboutDialog()
{
    delete ui;
}

void AboutDialog::on_buttonBox_accepted()
{
    close();
}
