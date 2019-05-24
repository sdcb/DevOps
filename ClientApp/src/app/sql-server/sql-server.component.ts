import { Component, OnInit, ViewChild, TemplateRef } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { MatDialog, MatDialogRef } from '@angular/material';
import { FormControl, Validators } from '@angular/forms';

@Component({
  selector: 'app-sql-server',
  templateUrl: './sql-server.component.html',
  styleUrls: ['./sql-server.component.css']
})
export class SqlServerComponent implements OnInit {
  dbList: Array<DatabaseDto>;
  @ViewChild('branchDialog') branchDialogView: TemplateRef<any>;
  @ViewChild('dropDialog') deleteDialogView: TemplateRef<any>;

  dbFrom: string;
  dbTo = new FormControl(null, [
    Validators.required,
    c => c.value === this.dbFrom ? { same: { value: c.value } } : null
  ]);
  dbToDrop: string;
  branchDialog: MatDialogRef<any>;
  deleteDialog: MatDialogRef<any>;

  constructor(
    private http: HttpClient,
    private dialog: MatDialog) {
  }

  loadData() {
    this.http.get<Array<DatabaseDto>>('/api/sqlServer/dbList').subscribe(data => this.dbList = data);
  }

  ngOnInit() {
    this.loadData();
  }

  openBranchDialog(item: DatabaseDto) {
    this.dbFrom = item.name;
    this.branchDialog = this.dialog.open(this.branchDialogView, { width: '500px' });
  }

  openDropDialog(item: DatabaseDto) {
    this.dbToDrop = item.name;
    this.deleteDialog = this.dialog.open(this.deleteDialogView, { width: '500px' });
  }

  branch() {
    if (!this.dbTo.valid) { return; }
    const url = `/api/sqlServer/branch?dbFrom=${encodeURIComponent(this.dbFrom)}&dbTo=${encodeURIComponent(this.dbTo.value)}`;
    this.http.post(url, null).subscribe(() => {
      this.loadData();
      this.branchDialog.close();
    });
  }

  delete() {
    const url = `/api/sqlServer/drop?dbToDrop=${encodeURIComponent(this.dbToDrop)}`;
    this.http.post(url, null).subscribe(() => {
      this.loadData();
      this.deleteDialog.close();
    });
  }
}

interface DatabaseDto {
  id: string;
  name: string;
}
